/*
 * Copyright (c) Microsoft Corporation.
 * Licensed under the MIT license.
 */

import * as React from 'react';
import Header from './Header';
import HeroList from './HeroList';

import { Spinner, SpinnerType } from 'office-ui-fabric-react';

export interface ProgressProps {
    logo: string;
    message: string;
    title: string;
}

export default class Progress extends React.Component<ProgressProps> {
    render() {
        const {
            logo,
            message,
            title,
        } = this.props;

        return (
                    <section className="ms-welcome__progress ms-u-fadeIn500">
                    <Header logo={logo} title={title} message="Welcome TypeScript" />
                    <HeroList
                        message="Discover what Office .NET Core 3.1 Add-ins can do for you today!"
                        items={[]}
                    >
                        <Spinner type={SpinnerType.large} label={message} />
                    </HeroList>
              </section>
        );
    }
}
